<template>
  <article class="mt-[20px] bg-bgColorPrimary theme-icon-secondary content-padding">
    <div class="flex flex-col items-center justify-center">

      <!--Locations Tab-->
      <locations-tab/>

      <!--Locations Grid-->
      <locations-grid/>
    </div>
  </article>
</template>
<script setup lang="ts">
import LocationsTab from "@/modules/lists/components/LocationsTab.vue";
import LocationsGrid from "@/modules/lists/components/LocationsGrid.vue";
</script>